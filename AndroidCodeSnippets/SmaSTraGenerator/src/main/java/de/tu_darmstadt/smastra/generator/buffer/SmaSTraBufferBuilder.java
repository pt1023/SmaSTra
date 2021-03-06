package de.tu_darmstadt.smastra.generator.buffer;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;

import de.tu_darmstadt.smastra.generator.ElementGenerationFailedException;
import de.tu_darmstadt.smastra.generator.elements.ProxyPropertyObj;
import de.tu_darmstadt.smastra.generator.extras.AbstractSmaSTraExtra;
import de.tu_darmstadt.smastra.markers.elements.config.ConfigurationElement;

/**
 * Builder pattern for a SmaSTra Sensor.
 *
 * @author Tobias Welther
 */
public class SmaSTraBufferBuilder {

    /**
     * The Other classes needed.
     */
    private final List<Class<?>> needsOtherClasses = new ArrayList<>();

    /**
     * The List of the configuration.
     */
    private final List<ConfigurationElement> configuration = new ArrayList<>();

    /**
     * The Proxy properties to use.
     */
    private final List<ProxyPropertyObj> proxyProperties = new ArrayList<>();

    /**
     * The extras to bundle in.
     */
    private final List<AbstractSmaSTraExtra> extras = new ArrayList<>();

    /**
     * The Description of the Transaction.
     */
    private String description = "";

    /**
     * The Name of the Method.
     */
    private Class<?> clazz = null;

    /**
     * The DisplayName to use.
     */
    private String displayName;

    /**
     * The Name of the Method to get the Data from.
     */
    private String bufferAddMethodName;

    /**
     * The Name of the Method to get the Data from.
     */
    private String bufferGetMethodName;



    public SmaSTraBufferBuilder setDescription(String description) {
        this.description = description;
        return this;
    }

    public SmaSTraBufferBuilder setClass(Class<?> clazz) {
        this.clazz = clazz;
        return this;
    }

    public SmaSTraBufferBuilder addNeededClass(Collection<Class<?>> otherClasses){
        this.needsOtherClasses.addAll(otherClasses);
        return this;
    }

    public SmaSTraBufferBuilder addNeededClass(Class<?>[] otherClasses){
        this.needsOtherClasses.addAll(Arrays.asList(otherClasses));
        return this;
    }

    public SmaSTraBufferBuilder setDisplayName(String displayName) {
        this.displayName = displayName;
        return this;
    }

    public SmaSTraBufferBuilder setBufferAddMethodName(String bufferAddMethodName) {
        this.bufferAddMethodName = bufferAddMethodName;
        return this;
    }

    public SmaSTraBufferBuilder setBufferGetMethodName(String bufferGetMethodName) {
        this.bufferGetMethodName = bufferGetMethodName;
        return this;
    }

    public SmaSTraBufferBuilder addConfigurationElements(List<ConfigurationElement> elements){
        if(elements != null) this.configuration.addAll(elements);
        return this;
    }

    public SmaSTraBufferBuilder addProxyProperties(List<ProxyPropertyObj> proxyProperties){
        if(proxyProperties != null) this.proxyProperties.addAll(proxyProperties);
        return this;
    }

    public SmaSTraBufferBuilder addExtra(AbstractSmaSTraExtra extra){
        if(extra != null) this.extras.add(extra);
        return this;
    }

    public SmaSTraBufferBuilder addExtras(Collection<AbstractSmaSTraExtra> extras){
        if(extras != null) this.extras.addAll(extras);
        return this;
    }

    public List<Class<?>> getNeedsOtherClasses() {
        return needsOtherClasses;
    }

    public String getDescription() {
        return description;
    }

    public Class<?> getClazz() {
        return clazz;
    }

    public String getDisplayName() {
        return displayName;
    }

    public String getBufferAddMethodName() {
        return bufferAddMethodName;
    }

    public String getBufferGetMethodName() {
        return bufferGetMethodName;
    }

    public List<ConfigurationElement> getConfiguration() {
        return configuration;
    }


    /**
     * Generates the Sensor.
     *
     * @throws ElementGenerationFailedException when essentials parts are missing (class, display name)
     * @return the generated Sensor.
     */
    public SmaSTraBuffer build() throws ElementGenerationFailedException {
        if(clazz == null) throw new ElementGenerationFailedException("No class defined.");
        if(displayName == null) throw new ElementGenerationFailedException("No displayName defined.");
        if(bufferAddMethodName == null) throw new ElementGenerationFailedException("No BufferAdd defined.");
        if(bufferGetMethodName == null) throw new ElementGenerationFailedException("No BufferGet defined.");

        return new SmaSTraBuffer(displayName, description, needsOtherClasses,
                bufferAddMethodName, bufferGetMethodName, clazz,
                configuration, proxyProperties, extras);
    }
}
